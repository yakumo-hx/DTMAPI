using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace Experiment.Doloc25DQa
{
    // Internal observer only. No automatic loading, deadline or save interception
    // is compiled into the ordinary player product.
    public sealed class ModEntry : DtmMod, IDisposable
    {
        private static ModEntry? current;
        private readonly Harmony guard = new Harmony("Experiment.Doloc25DQa.Native");
        private Settings settings = null!;
        private float started, stableAt, quitAt;
        private int ticks, loads, saves, rejectedSaves, shotFrame;
        private string room = "";
        private string? pendingShot;
        private bool loaded, outdoor, ready, disposed, oldBackground;
        private bool clockCaptured, oldClockPause;
        private float traceUntil;
        private object? lastRuntime;
        private double frameSum, frameMax;
        private long frameSamples;
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private object? Product => AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "DTMAPI.Doloc25D")?
            .GetType("DTMAPI.Doloc25D.ModEntry")?.GetField("Current", Flags)?.GetValue(null);
        private object? Runtime => Product?.GetType().GetField("Runtime", Flags)?.GetValue(Product);
        private object? Config => Product?.GetType().GetField("Config", Flags)?.GetValue(Product);
        private object? Invoke(string name) { var r = Runtime; return r?.GetType().GetMethod(name, Flags)?.Invoke(r, null); }
        private void Set(string name, object value) { var c = Config; if (c == null) throw new InvalidOperationException("Product missing."); c.GetType().GetProperty(name, Flags)!.SetValue(c, value); Invoke("ClearFault"); }
        public override void Entry(IDtmHelper helper)
        {
            settings = helper.ReadConfig<Settings>(); current = this;
            if (!Path.IsPathRooted(settings.Evidence) || DateTime.Parse(settings.ExpiresUtc).ToUniversalTime() < DateTime.UtcNow)
                throw new InvalidOperationException("Fresh bounded observer settings required.");
            Directory.CreateDirectory(settings.Evidence); started = Time.realtimeSinceStartup;
            oldBackground = Application.runInBackground; Application.runInBackground = true;
            var reject = new HarmonyMethod(typeof(ModEntry).GetMethod(nameof(RejectSave)));
            guard.Patch(typeof(DolocAPI).GetMethod("SaveGame", new[] { typeof(int) }), prefix: reject);
            guard.Patch(typeof(DolocTown.GameData.DataPersistenceManager).GetMethod("SaveGame", new[] { typeof(int) }), prefix: reject);
            helper.Events.GameLoop.UpdateTicked += Tick;
            helper.Events.Save.SaveLoaded += (_, e) => { ++loads; Write("loaded.json", new { loads }); };
            helper.Events.Save.SaveSaved += (_, e) => { ++saves; Write("unexpected-save.json", new { saves }); };
            helper.Events.GameLoop.ReturnedToTitle += (_, e) => { ready = false; room = ""; };
            Write("entry.json", new { settings, NativeIndex = 1, SaveMode = "NoNativeSave", OriginalRunInBackground = oldBackground });
        }
        public static bool RejectSave(ref bool __result) { __result = false; if (current != null) { ++current.rejectedSaves; current.Write("save-blocked.json", new { current.rejectedSaves }); } return false; }
        private void Tick(object sender, UpdateTickedEventArgs e)
        {
            try
            {
                ++ticks; float now = Time.realtimeSinceStartup;
                if (quitAt > 0) { if (now >= quitAt) Application.Quit(); return; }
                if (now - started > settings.TimeoutSeconds) { Quit("deadline"); return; }
                if (ready) { double dt = Time.unscaledDeltaTime * 1000; frameSum += dt; frameMax = Math.Max(frameMax, dt); ++frameSamples; }
                if (ready && now < traceUntil && ticks % 2 == 0) File.AppendAllText(Path.Combine(settings.Evidence,"motion.jsonl"), JsonConvert.SerializeObject(Status()) + "\n");
                if (pendingShot != null && Time.frameCount >= shotFrame) { Snapshot(pendingShot); pendingShot = null; }
                if (ticks % 15 != 0) return;
                var state = DolocAPI.userInput?.CurrentState?.GetType().Name ?? "";
                if (!loaded && state == "HomePageUiState" && now - started > 12)
                {
                    loaded = true;
                    typeof(DolocAPI).GetMethod("Command_LoadGame", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(int) }, null)!.Invoke(null, new object[] { 1 });
                    Write("load-request.json", new { PhysicalInput = false, Entry = "DolocAPI.Command_LoadGame(1)" });
                }
                if (loads > 0 && DolocAPI.CurrentRoom != null && DolocAPI.IsNormalState)
                {
                    if (!outdoor)
                    {
                        outdoor = true;
                        var house = DolocAPI.CurrentRoom as DolocTown.TemplateRoomInHouse;
                        if (house != null && house.RootRoom != house) { DolocAPI.EnterRoom(house.RootRoom, house.Building.EntryPosition); stableAt = now + 4; return; }
                    }
                    string next = DolocAPI.CurrentRoom.RoomId;
                    if (room != next) { ready = false; room = next; stableAt = now + 4; }
                    if (!ready && now >= stableAt)
                    {
                        ready = true;
                        if (settings.FreezeClock && !clockCaptured) { clockCaptured=true; oldClockPause=DolocAPI.gameLoop.IsGlobalPaused; DolocAPI.gameLoop.IsGlobalPaused=true; Write("clock-setup.json",new{PhysicalInput=false,OriginalPaused=oldClockPause,Paused=true}); }
                        Write("ready.json", Status()); Write("inventory.json", Invoke("Inventory")); Shot("00-initial");
                    }
                }
                lastRuntime = Invoke("Diagnostics");
                Write("status.json", Status());
                string commandFile = Path.Combine(settings.Evidence, "command.txt");
                if (!File.Exists(commandFile)) return;
                string command = File.ReadAllText(commandFile).Trim(); File.Delete(commandFile);
                Command(command); Write("command-done.json", new { Command = command, Frame = Time.frameCount, Utc = DateTime.UtcNow });
            }
            catch (Exception ex) { Write("failure.json", new { Error = ex.ToString() }); Quit("failure"); }
        }
        private void Command(string command)
        {
            var p = command.Split(':');
            switch (p[0])
            {
                case "quit": Quit("command"); break;
                case "original": Set("Enabled", false); break;
                case "on": Set("Enabled", true); break;
                case "angle": Set("Yaw", double.Parse(p[1], CultureInfo.InvariantCulture)); Set("Pitch", double.Parse(p[2], CultureInfo.InvariantCulture)); Set("ViewScale", double.Parse(p[3], CultureInfo.InvariantCulture)); Set("Enabled", true); break;
                case "snap": Shot(p[1]); break;
                case "inventory": Write("inventory.json", Invoke("Inventory")); break;
                case "perf-reset": frameSum = frameMax = 0; frameSamples = 0; break;
                case "trace": traceUntil = Time.realtimeSinceStartup + 15; break;
                case "clock": DolocAPI.gameLoop.IsGlobalPaused = p[1] == "pause"; break;
                case "prepare-build":
                    var give = typeof(DolocAPI).GetMethod("Command_GenItem", BindingFlags.Static|BindingFlags.NonPublic, null, new[]{typeof(string),typeof(int)}, null)!;
                    give.Invoke(null,new object[]{"construct_controller",1}); give.Invoke(null,new object[]{"pt_wood",10});
                    Write("construction-setup.json",new { PhysicalInput=false,Items=new[]{"construct_controller:1","pt_wood:10"},SaveMode="NoNativeSave",Purpose="Inventory setup only; activation/preview/placement through physical input" }); break;
                case "reload": DolocAPI.ReturnHome(); loaded = false; outdoor = false; ready = false; break;
                default: throw new ArgumentException(command);
            }
        }
        private void Shot(string label) { if (label.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) throw new ArgumentException(label); pendingShot = label; shotFrame = Time.frameCount + 4; }
        private void Snapshot(string label)
        {
            Write(label + ".json", Status());
            Write(label + ".audit.json", new {
                VisualRoots = UnityEngine.Object.FindObjectsOfType<Transform>().Count(t=>t.name=="Doloc25D.Visuals"),
                ProductHooks = Harmony.GetAllPatchedMethods().Count(m=>Harmony.GetPatchInfo(m).Owners.Contains("DTMAPI.Doloc25D.Native")),
                Platforms = DolocAPI.CurrentRoom?.DM_platform.totalPlatforms.Select(p=>new{p.proto.Id,p.geometry.Left,p.geometry.Right,p.geometry.Height,ColumnLeft=p.geometry.columnHeight.x,ColumnRight=p.geometry.columnHeight.y}).ToArray(),
                SelectedItem = DolocAPI.CurrentRoom != null ? DolocAPI.SelectedItem?.GetType().Name : null });
            ScreenCapture.CaptureScreenshot(Path.Combine(settings.Evidence, label + ".png"));
        }
        private object Status()
        {
            bool inRoom = DolocAPI.CurrentRoom != null && DolocAPI.agent != null && DolocAPI.agent.Status != null;
            Vector2 mouse = DolocAPI.UserInput != null ? DolocAPI.UserInput.MousePosition : Vector2.zero; Vector2 world = inRoom ? DolocAPI.ScreenToWorld(mouse) : Vector2.zero;
            return new { Utc = DateTime.UtcNow, ticks, Frame = Time.frameCount, ready, room, loads, saves, rejectedSaves,
                NativeState = DolocAPI.userInput?.CurrentState?.GetType().Name, NativeAgent = inRoom ? V(DolocAPI.AgentPosition) : null,
                Velocity = inRoom ? V(DolocAPI.agent.Velocity) : null, Mouse = V(mouse), MouseWorld = V(world),
                Grounded = inRoom && DolocAPI.agent.Status.IsGrounded,
                MouseRoundtrip = inRoom ? V(DolocAPI.WorldToScreen(world)) : null, Runtime = Invoke("Diagnostics"),
                Perf = new { Samples = frameSamples, MeanFrameMs = frameSamples == 0 ? 0 : frameSum / frameSamples, MaxFrameMs = frameMax } };
        }
        private static object V(Vector3 v) => new { v.x, v.y, v.z };
        private void Write(string file, object? value) => File.WriteAllText(Path.Combine(settings.Evidence, file), JsonConvert.SerializeObject(value, Formatting.Indented));
        private void Quit(string reason) { quitAt = Time.realtimeSinceStartup + 2; lastRuntime = Invoke("Diagnostics"); Write("before-quit.json", new { Reason = reason, State = Status() }); }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            guard.UnpatchSelf(); Application.runInBackground = oldBackground; if (clockCaptured) DolocAPI.gameLoop.IsGlobalPaused=oldClockPause; current = null;
            Write("dispose.json", new { loads, saves, rejectedSaves, LastRuntime = lastRuntime,
                RemainingHooks = Harmony.GetAllPatchedMethods().Count(m => Harmony.GetPatchInfo(m).Owners.Contains("Experiment.Doloc25DQa.Native")) });
        }
    }
    public sealed class Settings
    { public string Evidence { get; set; } = ""; public string ExpiresUtc { get; set; } = ""; public int TimeoutSeconds { get; set; } = 900; public bool FreezeClock { get; set; } = true; }
}
