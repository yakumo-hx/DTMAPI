using System;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Zoom;
using HarmonyLib;
using UnityEngine;
namespace DTMAPI.ZoomHarmonyOwnerFixture
{
    internal static class Program
    {
        private static int checks;
        private static int Main(string[] args)
        {
            if (args.Length != 0 && args[0] == "--hang-fixture") { System.Threading.Thread.Sleep(30000); return 0; }
            if (args.Length != 0 && args[0] == "--output-flood-fixture") { Console.Write(new string('x', 140000)); Console.Error.Write(new string('x', 140000)); return 0; }
            try
            {
                AccurateObjects(); Refresh(); Sprites(); FailureCleanup(); HookSets(); Input(); OwnerLifetime.Check();
                Console.WriteLine("ZoomHarmonyOwnerFixture: OK checks=" + checks); return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
            finally { Harmony.UnpatchID(ZoomProductContract.HarmonyOwner); Harmony.UnpatchID("zoom.test.unrelated"); Harmony.UnpatchID(ZoomProductContract.CompatibilityOwner); }
        }
        private static ZoomNativeRuntime New(out ZoomHookInstaller hooks)
        {
            Harmony.UnpatchID(ZoomProductContract.HarmonyOwner);
            DolocAPI.mainCamera = new Camera(); DolocAPI.cameraController = new DolocTown.CameraController();
            DolocAPI.envBackgroundEx = new DolocTown.BackgroundRenderer(); DolocAPI.EnvCovariantController = new DolocTown.EnvCovariantController();
            hooks = new ZoomHookInstaller(NullMonitor.Instance);
            var runtime = new ZoomNativeRuntime(NullMonitor.Instance, hooks);
            runtime.Configure(new ZoomConfig { MaxViewScale = 10 }, "fixture"); return runtime;
        }
        private static void AccurateObjects()
        {
            var r = New(out var hooks); var a = DolocAPI.mainCamera;
            Check(r.SetViewScale(4, "a")); Eq(a.orthographicSize, 40);
            var b = new Camera { orthographicSize = 7 }; DolocAPI.mainCamera = b;
            r.Tick(); Eq(a.orthographicSize, 10); Eq(b.orthographicSize, 28);
            r.ResetToVanilla("reset"); Eq(b.orthographicSize, 7);
            r.SetViewScale(4, "again"); b.orthographicSize = 31;
            r.OnEnvironmentReset(); Eq(b.orthographicSize, 31); Eq(r.CurrentViewScale, 1);
            r.Tick(); Eq(b.orthographicSize, 31);
            r.SetViewScale(4, "new explicit demand"); b.Destroyed = true;
            DolocAPI.mainCamera = new Camera { orthographicSize = 12 };
            r.Tick(); Eq(DolocAPI.mainCamera.orthographicSize, 48);
            r.DeactivateOwner("done"); Eq(DolocAPI.mainCamera.orthographicSize, 12); Check(!hooks.HasOwnedPatches);
        }
        private static void Refresh()
        {
            var r = New(out _); var cam = DolocAPI.mainCamera; var controller = DolocAPI.cameraController;
            r.SetViewScale(4, "refresh"); int calls = controller.Calls;
            r.SetViewScale(10, "no native refresh"); Eq(controller.Calls, calls);
            var outer = r.BeforeNativeRefreshResolution(controller); Eq(cam.orthographicSize, 10);
            var inner = r.BeforeNativeRefreshResolution(controller); Eq(cam.orthographicSize, 10);
            var native = new ApplicationException("native");
            Check(ReferenceEquals(r.AfterNativeRefreshResolution(inner, native), native)); Eq(cam.orthographicSize, 10);
            Check(ReferenceEquals(r.AfterNativeRefreshResolution(outer, native), native)); Eq(cam.orthographicSize, 100);
            controller.RefreshResolution(); Eq(controller.Observed, 10); Eq(cam.orthographicSize, 100);
            controller.DuringRefresh = () => { throw native; };
            try { controller.RefreshResolution(); throw new Exception("no native exception"); } catch (ApplicationException ex) { Check(ReferenceEquals(ex, native)); }
            Eq(cam.orthographicSize, 100); controller.DuringRefresh = null;
            outer = r.BeforeNativeRefreshResolution(controller); r.ResetToVanilla("reset inside");
            r.AfterNativeRefreshResolution(outer, native); Eq(cam.orthographicSize, 10); Eq(r.CurrentViewScale, 1);
            r.SetViewScale(4, "replace inside"); outer = r.BeforeNativeRefreshResolution(controller);
            var replacement = new Camera { orthographicSize = 6 }; DolocAPI.mainCamera = replacement;
            r.AfterNativeRefreshResolution(outer, native); Eq(replacement.orthographicSize, 6); Eq(cam.orthographicSize, 10);
            r.Tick(); Eq(replacement.orthographicSize, 24);
            outer = r.BeforeNativeRefreshResolution(controller); replacement.orthographicSize = 19;
            r.AfterNativeRefreshResolution(outer, native); Eq(replacement.orthographicSize, 19); Eq(r.CurrentViewScale, 1);
            r.DeactivateOwner("done");
        }
        private static void Sprites()
        {
            var r = New(out _); var bg = DolocAPI.envBackgroundEx; var layer = bg.Layer.SpriteRenderer;
            layer.transform.localScale = new Vector3(2, 3, 9); layer.transform.localPosition = new Vector3(13, 17, 19);
            var material = layer.material;
            r.SetViewScale(4, "background"); Eq(layer.transform.localScale.x, 8); Eq(layer.transform.localScale.y, 12);
            Eq(layer.transform.localScale.z, 9); Eq(layer.transform.localPosition.y, 17); Check(ReferenceEquals(material, layer.material)); Eq(layer.color, 42);
            var fog = DolocAPI.EnvCovariantController; Eq(fog.Outdoor.Sprites[0].transform.localScale.x, 4); Eq(fog.Indoor.Sky.transform.localScale.y, 4);
            long discovers = r.DiscoveryCount, writes = r.ScaleWriteCount;
            for (int i = 0; i < 1000; ++i) r.Tick(); Eq(r.DiscoveryCount, discovers); Eq(r.ScaleWriteCount, writes);
            bg.LoadPreset(bg.CurrentPreset); r.Tick(); Eq(layer.transform.localScale.x, 8);
            layer.transform.FailWrite = true;
            bg.LoadPreset(new DolocTown.GameData.EnvBackgroundSO()); r.Tick(); Eq(layer.transform.localScale.x, 8);
            layer.transform.FailWrite = false; r.Tick(); Eq(layer.transform.localScale.x, 2);
            r.Tick(); Eq(layer.transform.localScale.x, 8);
            bg.LoadPreset(new DolocTown.GameData.EnvBackgroundSO()); r.Tick(); Eq(layer.transform.localScale.y, 12);
            layer.sprite = new Sprite(); r.Tick(); Eq(layer.transform.localScale.y, 12);
            DolocAPI.mainCamera.aspect *= 2; r.Tick(); Eq(layer.transform.localScale.x, 16); Eq(layer.transform.localScale.y, 12);
            layer.transform.localScale = new Vector3(91, 92, 99); r.Tick(); r.SetViewScale(10, "external scale"); Eq(layer.transform.localScale.x, 91);
            r.ResetToVanilla("return"); Eq(layer.transform.localScale.y, 92); Eq(layer.transform.localScale.z, 99); Eq(fog.Outdoor.Sprites[0].transform.localScale.x, 1);
            discovers = r.DiscoveryCount; writes = r.ScaleWriteCount; r.OnEnvironmentReset(); r.Tick(); Eq(r.DiscoveryCount, discovers); Eq(r.ScaleWriteCount, writes);
            r.DeactivateOwner("done");
        }
        private static void FailureCleanup()
        {
            var r = New(out var hooks); var cam = DolocAPI.mainCamera; var fog = DolocAPI.EnvCovariantController.Outdoor.Sprites[0];
            r.SetViewScale(4, "failure"); cam.FailWrite = true; fog.transform.FailWrite = true;
            Throws(() => r.DeactivateOwner("fails")); Check(!hooks.HasOwnedPatches); Check(r.PendingRestoreCount == 2);
            Eq(DolocAPI.envBackgroundEx.Sky.SpriteRenderer.transform.localScale.x, 1);
            cam.FailWrite = false; fog.transform.FailWrite = false; r.DeactivateOwner("retry"); Eq(cam.orthographicSize, 10); Eq(fog.transform.localScale.x, 1); Eq(r.PendingRestoreCount, 0);
            r = New(out hooks); cam = DolocAPI.mainCamera; r.SetViewScale(4, "read fail"); cam.FailRead = true;
            Throws(() => r.DeactivateOwner("read")); Check(!hooks.HasOwnedPatches); Eq(r.PendingRestoreCount, 1);
            cam.FailRead = false; r.DeactivateOwner("retry"); Eq(cam.orthographicSize, 10);
        }
        private static void HookSets()
        {
            var r = New(out var hooks); Check(hooks.IsInstalled); Eq(hooks.InstalledPatchCount, 7);
            var observer = new Harmony("zoom.test.unrelated"); var noop = new HarmonyMethod(typeof(Program).GetMethod(nameof(Noop), BindingFlags.Static | BindingFlags.NonPublic));
            observer.Patch(ZoomHookInstaller.ResolveTarget(), postfix: noop);
            var owner = new Harmony(ZoomProductContract.HarmonyOwner);
            owner.Unpatch(ZoomHookInstaller.ResolveRefreshResolutionTarget(), HarmonyPatchType.Finalizer, ZoomProductContract.HarmonyOwner);
            Check(!hooks.IsInstalled && hooks.HasOwnedPatches);
            r.DeactivateOwner("partial"); Check(!hooks.HasOwnedPatches); Check(ZoomHookInstaller.HasExactOwner(ZoomHookInstaller.ResolveTarget(), "zoom.test.unrelated")); observer.UnpatchSelf();
            foreach (int residue in new[] { 1, 2 })
            {
                int count = 0; bool detached = false;
                Throws(() => ZoomHookTransaction.Install(() => { }, () => { count = residue; throw new ApplicationException(); }, () => count == 7, () => count > 0, () => { }, () => detached = true));
                Check(detached && count == residue);
                owner.Patch(ZoomHookInstaller.ResolveTarget(), postfix: noop);
                if (residue == 2) owner.Patch(ZoomHookInstaller.ResolveRefreshResolutionTarget(), prefix: noop);
                var failed = new ZoomHookInstaller(NullMonitor.Instance, unpatchOverride: () => { });
                Check(!failed.IsInstalled && failed.HasOwnedPatches); Throws(() => failed.UnpatchOwnedHooks(r));
                owner.UnpatchSelf();
            }
            new Harmony(ZoomProductContract.CompatibilityOwner).Patch(ZoomHookInstaller.ResolveTarget(), postfix: noop);
            Throws(() => new ZoomHookInstaller(NullMonitor.Instance).InstallAtomically(r));
            Harmony.UnpatchID(ZoomProductContract.CompatibilityOwner);
        }
        private static void Input()
        {
            var config = new ZoomConfig(); Eq(config.Step, 0.5); Eq(config.MaxViewScale, 4);
            var old = ReadConfig("{\"MaxViewScale\":4,\"Step\":0.25,\"IncreaseKey\":\"F7\",\"DecreaseKey\":\"F8\"}").Copy();
            Eq(old.Step, 0.25); Check(old.IncreaseKey == "F7" && old.DecreaseKey == "F8");
            Check(new ZoomInputBindings(old).Values[0].IsBound && new ZoomInputBindings(old).Values[1].IsBound);
            var unbound = ReadConfig("{\"MaximumKey\":\"\",\"ResetKey\":\"\"}").Copy();
            Check(!new ZoomInputBindings(unbound).Values[0].IsBound && !new ZoomInputBindings(unbound).Values[1].IsBound);
            Eq(new ZoomConfig { Step = 0.25 }.Copy().Step, 0.25); Eq(new ZoomConfig { MaxViewScale = 20, Step = 3 }.Copy().MaxViewScale, 10); Eq(new ZoomConfig { Step = 3 }.Copy().Step, 2);
            var bindings = new ZoomInputBindings(config);
            foreach (string key in new[] { "Equals", "KeypadPlus" }) { Eq(Select(bindings, key), 2); Eq(Select(bindings, key, "LeftShift"), 0); Eq(Select(bindings, key, "RightShift"), 0); }
            foreach (string key in new[] { "Minus", "KeypadMinus" }) { Eq(Select(bindings, key), 3); Eq(Select(bindings, key, "LeftShift"), 1); }
            config.MaximumKey = "Control+M"; config.ResetKey = "R"; config.IncreaseKey = "M"; config.DecreaseKey = "N";
            bindings = new ZoomInputBindings(config); Eq(Select(bindings, "M", "LeftControl"), 0); Eq(Select(bindings, "M"), 2); Eq(Select(bindings, "R"), 1); Eq(Select(bindings, "N"), 3);
            Eq(bindings.Select(_ => false, _ => false, _ => false), -1);
        }
        private static int Select(ZoomInputBindings b, string key, string modifier = "") => b.Select(x => x == key, x => x == key || x == modifier, _ => false);
        private static ZoomConfig ReadConfig(string json)
        {
            // The SDK runner supplies the exact frozen Core; ordinary suite runs
            // resolve its own built Core, using the same production JSON entry.
            string corePath = Environment.GetEnvironmentVariable("ZOOM_TEST_CORE") ?? System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DTMAPI.Core.dll");
            var type = Assembly.LoadFrom(corePath).GetType("DTMAPI.Core.Json.JsonFile", true);
            return (ZoomConfig)type.GetMethod("Deserialize", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).MakeGenericMethod(typeof(ZoomConfig)).Invoke(null, new object[] { json });
        }
        private static void Noop() { }
        private static void Check(bool condition) { ++checks; if (!condition) throw new Exception("Check " + checks + " failed"); }
        private static void Eq(double actual, double expected) { ++checks; if (Math.Abs(actual - expected) > 0.0001) throw new Exception("Check " + checks + ": expected " + expected + ", got " + actual); }
        private static void Throws(Action action) { ++checks; try { action(); } catch { return; } throw new Exception("Expected failure at " + checks); }
    }
}
