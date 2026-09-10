using System;
using DTMAPI.Abstractions;
using DTMAPI.Core.Services;
using DTMAPI.GameBridge.DolocTown.Native;
using DTMAPI.BepInExBootstrap;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Manifesting;
using DTMAPI.ModConfigMenu;
using System.Reflection;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void TitleSettingsFitsChangingViewportAndDefersEntryUntilRestart()
        {
            foreach (var viewport in new[] { (640, 480), (1280, 720), (1920, 1080), (2560, 1080), (1080, 1920), (3840, 2160), (1280, 720) })
            {
                float scale = ReflectedTitleMenuSettingsUi.CalculateViewportScale(viewport.Item1, viewport.Item2);
                Assert(scale > 0 && ReflectedTitleMenuSettingsUi.PanelDesignWidth * scale <= viewport.Item1 * 0.7501f &&
                    ReflectedTitleMenuSettingsUi.PanelDesignHeight * scale <= viewport.Item2 * 0.8001f,
                    "Panel and hit targets fit both viewport axes, including after resolution changes.");
            }
            var registry = new ConfigMenuRegistry();
            var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), registry);
            runtime.Paths.Ensure();
            var ui = new ReflectedTitleMenuSettingsUi(runtime, registry);
            MethodInfo initialize = typeof(ReflectedTitleMenuSettingsUi).GetMethod("InitializeInputSettings", BindingFlags.NonPublic | BindingFlags.Instance)!;
            FieldInfo active = typeof(ReflectedTitleMenuSettingsUi).GetField("nativeTitleEntryEnabledAtStartup", BindingFlags.NonPublic | BindingFlags.Instance)!;
            initialize.Invoke(ui, null);
            Assert(!(bool)active.GetValue(ui)!, "Existing and fresh settings leave the parallel title entry disabled.");
            ((IConfigMenuRuntime)registry).BeginEditing("DTMAPI.TitleSettings");
            Assert(((IConfigMenuRuntime)registry).GetPage("DTMAPI.TitleSettings")!.Items[1].TrySetPendingValue("true", out _), "Enable the option through the same draft transaction as the UI.");
            ((IConfigMenuRuntime)registry).Save("DTMAPI.TitleSettings");
            initialize.Invoke(ui, null);
            Assert(!(bool)active.GetValue(ui)!, "Saving does not mutate the live native action list.");
            ui.Shutdown("restart fixture");
            ui = new ReflectedTitleMenuSettingsUi(runtime, registry);
            initialize.Invoke(ui, null);
            Assert((bool)active.GetValue(ui)!, "Restart reads the saved enabled entry.");
            ((IConfigMenuRuntime)registry).BeginEditing("DTMAPI.TitleSettings");
            Assert(((IConfigMenuRuntime)registry).GetPage("DTMAPI.TitleSettings")!.Items[1].TrySetPendingValue("false", out _), "Disable through the UI draft transaction.");
            ((IConfigMenuRuntime)registry).Save("DTMAPI.TitleSettings");
            Assert((bool)active.GetValue(ui)!, "Disabling also waits for restart.");
            ui.Shutdown("restart fixture");
            ui = new ReflectedTitleMenuSettingsUi(runtime, registry);
            initialize.Invoke(ui, null);
            Assert(!(bool)active.GetValue(ui)!, "Restart withdraws a disabled entry.");
            ui.Shutdown("test complete");
        }

        private static void NativeTitleEntryPreservesOtherActionsAndExit()
        {
            var first = new object(); var otherMod = new object(); var exit = new object(); var owned = new object();
            var actions = new System.Collections.ArrayList { first, otherMod, exit };
            bool IsExit(object item) => ReferenceEquals(item, exit);
            Assert(OwnedTitleMenuAction.Ensure(actions, owned, IsExit, out bool changed) && changed, "Install own action in native navigation.");
            Assert(ReferenceEquals(actions[0], first) && ReferenceEquals(actions[1], otherMod) && ReferenceEquals(actions[2], owned) && ReferenceEquals(actions[3], exit), "Preserve native/foreign dispatch order and Cancel's last-item exit.");
            for (int i = 0; i < 5; i++) Assert(OwnedTitleMenuAction.Ensure(actions, owned, IsExit, out changed) && !changed && actions.Count == 4, "Render and language refresh must not duplicate the entry.");
            actions.Insert(0, owned);
            Assert(OwnedTitleMenuAction.Ensure(actions, owned, IsExit, out changed) && changed && actions.Count == 4, "Repair only duplicate exact owned objects.");
            OwnedTitleMenuAction.Remove(actions, owned);
            Assert(actions.Count == 3 && ReferenceEquals(actions[1], otherMod) && ReferenceEquals(actions[2], exit), "Withdrawal leaves foreign actions and exit unchanged.");
            actions.Clear(); actions.Add(first); actions.Add(exit);
            Assert(OwnedTitleMenuAction.Ensure(actions, owned, IsExit, out changed) && changed, "A native rebuild can reuse the entry after clearing its list.");
            actions.Add(otherMod);
            Assert(!OwnedTitleMenuAction.Ensure(actions, owned, IsExit, out changed) && !changed && ReferenceEquals(actions[3], otherMod), "Unknown nonterminal exit shape is left unchanged.");
            OwnedTitleMenuAction.Remove(actions, owned);
            Assert(actions.Count == 3 && ReferenceEquals(actions[1], exit), "Withdrawal removes only own identity even after foreign rearrangement.");
            actions.Remove(exit);
            Assert(!OwnedTitleMenuAction.Ensure(actions, owned, IsExit, out _) && actions.Count == 2, "Missing native exit degrades without list mutation.");
        }

        private static void RuntimeInputConfigSerializesAndOwnsOnlyItsPage()
        {
            var registry = new ConfigMenuRegistry();
            var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), registry);
            runtime.Paths.Ensure();
            var ui = new ReflectedTitleMenuSettingsUi(runtime, registry);
            typeof(ReflectedTitleMenuSettingsUi).GetMethod("InitializeInputSettings", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(ui, null);
            var owner = new ManifestModel { UniqueID = "DTMAPI.TitleSettings", Version = "0.6.4" };
            var settings = runtime.Config.ReadConfig<ReflectedTitleMenuSettingsUi.InputSettings>(owner);
            Assert(settings.OpenConfig == "F8", "Runtime-owned config must serialize on the actual serializer path.");
            settings.OpenConfig = "Control+H, GamepadStart";
            runtime.Config.WriteConfig(owner, settings);
            Assert(runtime.Config.ReadConfig<ReflectedTitleMenuSettingsUi.InputSettings>(owner).OpenConfig == settings.OpenConfig, "Keyboard/controller alternatives survive a cold config read.");
            var missing = new ManifestModel { UniqueID = "Missing.Mod", Version = "1.0.0" };
            registry.Register(missing, () => { }, () => { });
            typeof(DtmApiRuntime).GetMethod("RefreshConfigPageLocks", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(runtime, null);
            var pages = (IConfigMenuRuntime)registry;
            Assert(!pages.GetPage(owner.UniqueID)!.IsLocked && pages.GetPage(missing.UniqueID)!.IsLocked, "Explicit Runtime ownership must not unlock missing product pages.");
            runtime.UI.SetUiContext("HomePageUiState", true, false, "input entry regression");
            var recordFrame = typeof(DtmApiRuntime).GetMethod("RecordInputFrame", BindingFlags.NonPublic | BindingFlags.Instance)!;
            void Sample(bool down) => recordFrame.Invoke(runtime, new object[] { new[] { new InputButtonSample("F8", down, down, !down) } });
            Sample(false);
            runtime.Update();
            Sample(true);
            ui.ProcessEntryAction();
            Assert(runtime.UI.IsOpen, "Title entry consumes the sampled F8 edge before runtime.Update clears it.");
            runtime.Update();
            runtime.UI.Close();
            ui.ProcessEntryAction();
            Assert(!runtime.UI.IsOpen, "A cleared edge cannot reopen the menu in a later frame.");
            pages.Save(owner.UniqueID);
            Sample(false);
            runtime.Update();
            Sample(true);
            ui.ProcessEntryAction();
            Assert(runtime.UI.IsOpen, "Saving the entry binding must retain its Title scope and allow reopening without restart.");
            runtime.Update();
            runtime.UI.Close();
            ui.Shutdown("test");
            Assert(pages.GetPage(owner.UniqueID) == null && runtime.Input.GetRegisteredButtons().Count == 0, "Input page and action roots close with their owner.");
        }

        private static void MenuNavigationWaitsForNeutralAndRepeatsWithoutCatchingUp()
        {
            var cycle = new MenuNavigationCycle();
            Assert(cycle.Advance(new MenuInput { Confirm = true }, 0, out bool confirm, out _) == 0 && !confirm, "Opening confirmation must not activate the first row.");
            cycle.Advance(default, 0.1, out _, out _);
            Assert(cycle.Advance(new MenuInput { X = 0.54 }, 0.2, out _, out _) == 0, "Stick must cross engage threshold.");
            Assert(cycle.Advance(new MenuInput { X = 0.6 }, 0.3, out _, out _) == 2, "Initial direction moves immediately.");
            Assert(cycle.Advance(new MenuInput { X = 0.4 }, 0.6, out _, out _) == 0, "Hysteresis keeps direction without early repeat.");
            Assert(cycle.Advance(new MenuInput { X = 0.4 }, 0.66, out _, out _) == 2, "First repeat follows 350ms delay.");
            Assert(cycle.Advance(new MenuInput { X = 0.4 }, 9, out _, out _) == 2, "A slow frame emits only one move.");
            Assert(cycle.Advance(new MenuInput { X = 0.4 }, 9.01, out _, out _) == 0, "No queued repeat burst after a stall.");
            cycle.Advance(new MenuInput { X = 0.3 }, 9.1, out _, out _);
            Assert(cycle.Advance(new MenuInput { X = 0.4 }, 9.2, out _, out _) == 0, "Neutral release restores engage threshold.");
            cycle.Advance(new MenuInput { ConfirmEdge = true }, 10, out confirm, out _);
            Assert(confirm, "Short native confirmation reaches the action layer.");
            cycle.Reset();
            cycle.Advance(new MenuInput { Cancel = true }, 11, out _, out bool cancel);
            Assert(!cancel, "Mode exit cannot immediately close another layer.");
        }

        private static void ConfigFocusGraphReachesControlsAcrossRowsAndColumns()
        {
            var barrier = new EditRenderBarrier();
            Assert(barrier.Advance(false), "Idle pages can render immediately.");
            barrier.Defer();
            for (int i = 0; i < 120; i++) Assert(!barrier.Advance(true), "A held click must retain its receiving button even across many frames.");
            Assert(!barrier.Advance(false), "Release frame must retain the button until EventSystem processes the click.");
            Assert(barrier.Advance(false), "Next frame can render committed edits and save results.");
            barrier.Defer();
            Assert(!barrier.Advance(false), "Keyboard end-edit also yields one update to pending UI events.");
            barrier.Reset();
            Assert(barrier.Advance(true), "Leaving the title clears an abandoned click barrier.");
            object value = new object();
            ReflectedTitleMenuSettingsUi.NavigationTarget Target(string name, float x, float y) => new ReflectedTitleMenuSettingsUi.NavigationTarget(name, value, value, value, value, x, y, false, null);
            var targets = new[] { Target("mod", 0, 0), Target("minus", 200, 0), Target("edit", 240, 0), Target("plus", 300, 0), Target("next row", 200, -32), Target("save", 200, 60) };
            Assert(ReflectedTitleMenuSettingsUi.FindDirectionalTarget(targets, 0, 2) == 1, "Right crosses from list into the nearest control.");
            Assert(ReflectedTitleMenuSettingsUi.FindDirectionalTarget(targets, 1, 2) == 2, "Adjacent numeric editor is reachable.");
            Assert(ReflectedTitleMenuSettingsUi.FindDirectionalTarget(targets, 2, 2) == 3, "Increment button is reachable without mouse.");
            Assert(ReflectedTitleMenuSettingsUi.FindDirectionalTarget(targets, 1, 4) == 4, "Down follows the control column.");
            Assert(ReflectedTitleMenuSettingsUi.FindDirectionalTarget(targets, 1, 3) == 5, "Save/Cancel toolbar remains reachable.");
            Assert(ReflectedTitleMenuSettingsUi.FindDirectionalTarget(targets, 0, 1) == -1, "Outer edge keeps focus instead of selecting hidden native UI.");
        }

        private static void ControllerReconnectRequiresNeutralAndClosesHeldCycles()
        {
            var cycle = new ControllerButtonCycle();
            cycle.Update(true, true, 1, 1, 0);
            Assert(cycle.Held == 0 && cycle.Pressed == 0, "Connecting with a held button must not trigger.");
            cycle.Update(true, false, 1, 0, 0);
            Assert(cycle.Held == 0, "Held connection state remains disarmed.");
            cycle.Update(true, false, 0, 0, 1);
            cycle.Update(true, false, 3, 3, 0);
            Assert(cycle.Held == 3 && cycle.Pressed == 3, "Fresh neutral-to-chord emits both buttons.");
            cycle.Update(true, false, 3, 3, 0);
            Assert(cycle.Pressed == 0, "Duplicate backend edges cannot repeat a held activation.");
            cycle.Update(false, false, 0, 0, 0);
            Assert(cycle.Released == 3 && cycle.Held == 0, "Focus loss/disconnect closes the prior physical cycle.");
            cycle.Update(true, false, 3, 0, 0);
            Assert(cycle.Pressed == 0 && cycle.Held == 0, "Focus return also waits for neutral.");
            cycle.Update(true, false, 0, 0, 0);
            cycle.Update(true, false, 0, 1, 1);
            Assert(cycle.Pressed == 1 && cycle.Released == 1, "A complete short tap remains observable.");
            for (int i = 0; i < 100; i++) cycle.Update(false, false, 0, 0, 0);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) cycle.Update(false, false, 0, 0, 0);
            Assert(GC.GetAllocatedBytesForCurrentThread() == before, "No-device steady state must allocate nothing.");
        }

        private static void ControllerVocabularyPreservesRawAndUnknownBindings()
        {
            string value = DtmKeybindList.Parse("Control+H, JoystickButton0, GamepadSouth, FuturePad17").ToString();
            Assert(value.Contains("JoystickButton0") && value.Contains("GamepadSouth") && value.Contains("FuturePad17"), "Raw, named and unknown repairable bindings must remain distinct.");
            Assert(DtmKeybindList.Parse(value).ToString() == value, "Old keyboard chords and controller alternatives must round-trip.");
            Assert(ReflectedUnityInput.FindUnrecognizedBindingButton(value) == "FuturePad17", "Unknown bindings need a repair hint without dropping the saved token.");
            Assert(ReflectedUnityInput.FindUnrecognizedBindingButton("Control+H, GamepadSouth, JoystickButton0") == string.Empty, "Known raw and named bindings are accepted without a connected device.");
            Assert(ReflectedUnityInput.FindUnrecognizedBindingButton("GamepadLeftTrigger") == "GamepadLeftTrigger", "Unsupported analog bindings must remain visibly unrecognized.");
            Assert(!ControllerButtonAdapter.TrySample("JoystickButton0", -1, out _), "Raw numeric IDs must never be translated into face-button aliases.");
            Assert(!ControllerButtonAdapter.TrySample("GamepadLeftTrigger", -1, out _), "Unproved analog binding must not become a digital button.");
            Assert(ControllerButtonAdapter.TrySample("GamepadSouth", -1, out InputButtonSample absent) && !absent.IsDownNow && !absent.PressedEdge,
                "No loaded Unity device backend must fail closed without inventing an activation.");
        }
    }
}
